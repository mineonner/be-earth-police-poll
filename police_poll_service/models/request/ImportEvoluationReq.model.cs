using System.ComponentModel.DataAnnotations;

namespace police_poll_service.models.request
{
    public class ImportEvoluationReqModel
    {
        [Required]
        public string org_unit_code { get; set; }
        [Required]
        public decimal service_work_score { get; set; }
        [Required]
        public decimal investigative_work_score { get; set; }
        [Required]
        public decimal crime_prevention_work_score { get; set; }
        [Required]
        public decimal traffic_work_score { get; set; }
        [Required]
        public decimal satisfaction_score { get; set; }
        [Required]
        public int service_work_count { get; set; }
        [Required]
        public int investigative_work_count { get; set; }
        [Required]
        public int crime_prevention_work_count { get; set; }
        [Required]
        public int traffic_work_count { get; set; }
        [Required]
        public int satisfaction_count { get; set; }
        [Required]
        public int evaluators_amount { get; set; }
        [Required]
        public string evaluation_year { get; set; }
    }
}
