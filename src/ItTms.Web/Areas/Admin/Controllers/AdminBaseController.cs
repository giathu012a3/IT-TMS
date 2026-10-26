using Microsoft.AspNetCore.Mvc;
using ItTms.Domain.Enums;
using ItTms.Web.Controllers;
using ItTms.Web.Filters;

namespace ItTms.Web.Areas.Admin.Controllers;

[Area("Admin")]
[AuthorizeRoles(RoleNames.Admin)]
public abstract class AdminBaseController : BaseController
{
}
