using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HelperClient.Models.FormModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelperClient.ViewModels
{
    internal partial class HomeViewModel : ObservableObject
    {
        // for data in form
        [ObservableProperty]
        private string _PhoneNumber = "";

        [ObservableProperty]
        private bool _isCustomErrorBoxEnable = false;

        [ObservableProperty]
        private string _customProblem = "";

        [ObservableProperty]
        private List<ProblemCategory> _problems = new();

        public List<string> GetSelectedProblems()
        {
            var selected = new List<string>();

            foreach (var problem in _problems)
            {
                foreach (var item in problem.Items.Where(i => i.IsChecked))
                    selected.Add(item.Name);
            }

            return selected;
        }

    }
}
