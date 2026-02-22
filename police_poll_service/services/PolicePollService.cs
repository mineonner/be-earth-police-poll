namespace police_poll_service.services
{
    public class PolicePollService
    {
        public double fixDigit(double value)
        {

            return Math.Floor(value * 100) / 100; ;
        }
    }
}
