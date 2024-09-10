using Humanizer;
using System.ComponentModel.DataAnnotations;

namespace WendlandtVentas.Infrastructure.Commons
{
    public enum ResultStatus
    {
        [Display(Name = "ok")] Ok,
        [Display(Name = "error")] Error,
        [Display(Name = "warning")] Warning
    }

    class AjaxResponse
    {
        public string Status { get; set; }
        public object Body { get; set; }
    }

    public static class AjaxFunctions
    {
        public static dynamic GenerateAjaxResponse(ResultStatus status, object body)
        {
            var response = new AjaxResponse();
            response.Status = status.Humanize();
            response.Body = body;

            return response;
        }

        public static dynamic GenerateJsonSuccess(object body)
        {
            return new
            {
                estado = ResultStatus.Ok.Humanize(),
                body
            };
        }

        public static dynamic GenerateJsonError(object body)
        {
            return new
            {
                estado = ResultStatus.Error.Humanize(),
                body
            };
        }
    }
}