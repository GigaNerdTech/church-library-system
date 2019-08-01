using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Church_Library_System
{
    public class PatronData
    {
        public string PatronID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int ItemsCheckedOut { get; set; }
        public bool IsMember { get; set; }

        public string FinesOwed { get; set; }

    }
}
