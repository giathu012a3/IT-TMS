using Microsoft.AspNetCore.Mvc;
using ItTms.Domain.Enums;
using ItTms.Web.Controllers;
using ItTms.Web.Filters;

namespace ItTms.Web.Areas.Staff.Controllers;

[Area("Staff")]
[AuthorizeRoles(RoleNames.Staff, RoleNames.Leader, RoleNames.Admin)]
public abstract class StaffBaseController : BaseController
{
}
