using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace police_poll_service.models.request
{
    public class ImportOrgUnitReqModel
    {
        [Required]
        public string org_unit_code { get; set; }
        [Required]
        public int service_work_total { get; set; }
        [Required]
        public int investigative_work_total { get; set; }
        [Required]
        public int crime_prevention_work_total { get; set; }
        [Required]
        public int traffic_work_total { get; set; }
        [Required]
        public int satisfaction_total { get; set; }
        [Required]
        public int evaluators_total { get; set; }
    }
}