﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransferDesk.Contracts.Manuscript.ComplexTypes.UserRole
{
    public class pr_GetAssociateInfo_Result
    {
        public int ID { get; set; }
        public string UserID { get; set; }
        public string empname { get; set; }
        public string email { get; set; }
    }
}
