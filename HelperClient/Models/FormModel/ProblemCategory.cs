using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelperClient.Models.FormModel
{
    internal class ProblemCategory
    {
        public string Title { get; set; }
        public List<ProblemItem> Items { get; set; }
    }
}
