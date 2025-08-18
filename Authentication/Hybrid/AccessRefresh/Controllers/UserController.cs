using AccessRefresh.Contracts.DTOs;
using AccessRefresh.Data.Entities;
using AccessRefresh.Domain.Filters;
using AccessRefresh.Extensions;
using AccessRefresh.Services.Application;
using Mapster;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AccessRefresh.Controllers;

[ApiController]
[Route(Constants.RouteDefault)]
[MinRole(EUserRole.User)]
public class UserController(UserService userService) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        return Ok(
            (await userService.GetUserById(HttpContext.GetUser()!.Id))
            .Adapt<UserDto>()
        );
    }   
}