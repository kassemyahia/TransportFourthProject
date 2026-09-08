using Microsoft.AspNetCore.Mvc;
using TransportFourthProject.Api.Enums;

using Microsoft.AspNetCore.Authorization;
using TransportFourthProject.Api.Authorization;

namespace TransportFourthProject.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = AppPolicies.UserOnly)]
    public class PaymentMethodController : ControllerBase
    {
        [HttpGet("options")]
        public IActionResult GetPaymentMethods()
        {
            var options = new[]
            {
                new { Key = PaymentMethod.FakeCard, Label = "Fake Card" },
                new { Key = PaymentMethod.CashAtOffice, Label = "Cash At Office" }
            };

            return Ok(options);
        }
    }
}
