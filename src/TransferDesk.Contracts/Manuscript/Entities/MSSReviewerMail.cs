﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TransferDesk.Contracts.Manuscript.Entities
{
    public class MSSReviewerMail
    {
        [Key]
        public int ID { get; set; }
        //public string Mail { get; set; }
        //public int ReviewerMasterID { get; set; }
        //public bool? IsActive { get; set; }
        //public string CreatedBy { get; set; }
        //public System.DateTime? CreatedDate { get; set; }
        //public string ModifiedBy { get; set; }
        //public System.DateTime? ModifiedDate { get; set; }
        public int? MSReviewersSuggestionInfoID { get; set; }
        public string Email { get; set; }

    }
}
