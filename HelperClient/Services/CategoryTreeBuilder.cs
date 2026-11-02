using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HelperClient.Models.DTO.CategoryDto;

namespace HelperClient.Services
{
    /// <summary>
    /// Узел вложенного дерева категорий проблем (для вывода списком с сохранением иерархии).
    /// </summary>
    public sealed class CategoryNode
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public int? ParentId { get; init; }

        public List<CategoryNode> Children { get; } = new();

        /// <summary>Используется ItemsControl для отступов вложенных элементов.</summary>
        public int Level { get; set; }

        /// <summary>Листовая категория — именно её можно выбрать для заявки.</summary>
        public bool IsLeaf => Children.Count == 0;

        public IEnumerable<CategoryNode> Flatten()
        {
            yield return this;
            foreach (var child in Children.SelectMany(c => c.Flatten()))
                yield return child;
        }
    }

    /// <summary>
    /// Построение дерева по плоскому списку категорий из /Category/GetAll.
    /// </summary>
    public static class CategoryTreeBuilder
    {
        /// <returns>Корневые узлы дерева; дочерние элементы вложены в Children.</returns>
        public static IReadOnlyList<CategoryNode> Build(IEnumerable<CategoryResponseDto> categories)
        {
            var nodes = categories
                .Where(c => c is not null && !string.IsNullOrWhiteSpace(c.Name))
                .Select(c => new CategoryNode
                {
                    Id = c.Id,
                    Name = c.Name.Trim(),
                    ParentId = c.ParentId,
                })
                .ToList();

            if (nodes.Count == 0) return Array.Empty<CategoryNode>();

            var byId = nodes.ToDictionary(n => n.Id);

            // Повторные ключи Id невозможны (уникальность на сервере), но защищаемся от некорректного ответа.
            var roots = new List<CategoryNode>();
            foreach (var node in nodes)
            {
                if (node.ParentId is int parentId && parentId != node.Id && byId.TryGetValue(parentId, out var parent))
                    parent.Children.Add(node);
                else
                    roots.Add(node);
            }

            // Защита от циклов: если родитель оказался внутри своего потомка — поднимаем узел в корень.
            BreakCycles(roots);

            AssignLevels(roots, 0);
            SortRecursive(roots);
            return roots;
        }

        private static void AssignLevels(IEnumerable<CategoryNode> nodes, int level)
        {
            foreach (var node in nodes)
            {
                node.Level = level;
                AssignLevels(node.Children, level + 1);
            }
        }

        private static void SortRecursive(List<CategoryNode> nodes)
        {
            nodes.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
            foreach (var node in nodes)
                SortRecursive(node.Children);
        }

        private static void BreakCycles(List<CategoryNode> roots)
        {
            var visiting = new HashSet<CategoryNode>();
            var visited = new HashSet<CategoryNode>();

            void Dfs(CategoryNode node)
            {
                if (!visiting.Add(node))
                {
                    // Найден цикл — разрываем его, перемещая узел в корень.
                    node.Children.Clear();
                    visited.Add(node);
                    visiting.Remove(node);
                    return;
                }

                foreach (var child in node.Children.ToList())
                {
                    if (visited.Contains(child)) continue;
                    Dfs(child);
                }

                visiting.Remove(node);
                visited.Add(node);
            }

            foreach (var root in roots.ToList())
                Dfs(root);
        }
    }
}
