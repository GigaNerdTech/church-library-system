using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Church_Library_System
{
    public class ItemData
    {
        public string ItemID { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public string Subject { get; set; }
        public string CallNo { get; set; }

        public string CheckedOut { get; set; }
        public string DueDate { get; set; }

        public string CheckedOutTo { get; set; }

    }
}
