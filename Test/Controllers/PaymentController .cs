using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Test.DbContext;
using Test.Dtos;
using Test.Entites;
using Test.FactoryPattern;

namespace Test.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly PaymentGatewayFactory _gatewayFactory;
        private readonly PaymentDbContext _context;

        public PaymentController(PaymentGatewayFactory gatewayFactory, PaymentDbContext context)
        {
            _gatewayFactory = gatewayFactory;
            _context = context;
        }

        [HttpPost("process")]
        public async Task<ActionResult<PaymentResult>> ProcessPayment([FromBody] CreatePaymentRequest request)
        {
            try
            {
                var userId = GetCurrentUserId(); // تحتاج تطبيق authentication
                var gateway = _gatewayFactory.GetGateway(request.Gateway);
                var result = await gateway.ProcessPaymentAsync(request, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpPost("save-payment-method")]
        public async Task<ActionResult<SavePaymentMethodResult>> SavePaymentMethod([FromBody] SavePaymentMethodRequest request)
        {
            try
            {
                var userId = GetCurrentUserId();
                var gateway = _gatewayFactory.GetGateway(request.Gateway);
                var result = await gateway.SavePaymentMethodAsync(request, userId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpGet("payment-methods")]
        public async Task<ActionResult<List<PaymentMethod>>> GetPaymentMethods()
        {
            var userId = GetCurrentUserId();
            var methods = await _context.PaymentMethods
                .Where(pm => pm.UserId == userId)
                .OrderByDescending(pm => pm.IsDefault)
                .ThenByDescending(pm => pm.CreatedAt)
                .ToListAsync();

            return Ok(methods);
        }
        [HttpGet("transactions")]
        public async Task<ActionResult<List<PaymentTransaction>>> GetTransactions()
        {
            var userId = GetCurrentUserId();
            var transactions = await _context.PaymentTransactions
                .Where(pt => pt.UserId == userId)
                .OrderByDescending(pt => pt.CreatedAt)
                .ToListAsync();

            return Ok(transactions);
        }

        [HttpPost("refund/{transactionId}")]
        public async Task<ActionResult<bool>> RefundPayment(string transactionId, [FromBody] decimal? amount = null)
        {
            var transaction = await _context.PaymentTransactions
                .FirstOrDefaultAsync(pt => pt.ExternalTransactionId == transactionId);

            if (transaction == null)
                return NotFound();

            var gateway = _gatewayFactory.GetGateway(transaction.Gateway);
            var result = await gateway.RefundPaymentAsync(transactionId, amount);

            if (result)
            {
                transaction.Status = TransactionStatus.Refunded;
                await _context.SaveChangesAsync();
            }

            return Ok(result);
        }
        private int GetCurrentUserId()
        {
            return 1; 
        }
    }
}
