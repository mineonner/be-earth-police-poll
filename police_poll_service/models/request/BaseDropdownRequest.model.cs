namespace police_poll_service.models.request
{
    public class BaseDropdownRequest
    {
        public int max_length { get; set; }
        public string[] selected_code { get; set; }
        public string search_text { get; set; }
        public string[] except_codes { get; set; }
    }
}
