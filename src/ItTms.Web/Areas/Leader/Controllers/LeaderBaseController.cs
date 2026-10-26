using Microsoft.AspNetCore.Mvc;
using ItTms.Domain.Enums;
using ItTms.Web.Controllers;
using ItTms.Web.Filters;

namespace ItTms.Web.Areas.Leader.Controllers;

[Area("Leader")]
[AuthorizeRoles(RoleNames.Leader, RoleNames.Admin)]
public abstract class LeaderBaseController : BaseController
{
}
