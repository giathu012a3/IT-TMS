using Microsoft.AspNetCore.Mvc;
using ItTms.Domain.Enums;
using ItTms.Web.Controllers;
using ItTms.Web.Filters;

namespace ItTms.Web.Areas.User.Controllers;

[Area("User")]
[AuthorizeRoles(RoleNames.User, RoleNames.Leader, RoleNames.Admin)]
public abstract class UserBaseController : BaseController
{
}
