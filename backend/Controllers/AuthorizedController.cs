using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GameAnalytics.Controllers;

[Authorize]
public abstract class AuthorizedController : ControllerBase { }
