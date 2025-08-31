using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webapi.Data;
using webapi.DTOs;
using webapi.Models;
using AutoMapper;
using Microsoft.AspNetCore.Identity;

namespace webapi.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PostsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PostsController> _logger;
    private readonly IMapper _mapper;
    private readonly UserManager<ApplicationUser> _userManager;

    public PostsController(
        ApplicationDbContext context, 
        ILogger<PostsController> logger,
        IMapper mapper,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _logger = logger;
        _mapper = mapper;
        _userManager = userManager;
    }

    // GET: api/posts
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PostDto>))]
    public async Task<ActionResult<IEnumerable<PostDto>>> GetPosts()
    {
        try
        {
            var posts = await _context.Posts
                .Include(p => p.User)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
                
            return Ok(_mapper.Map<IEnumerable<PostDto>>(posts));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting posts");
            return StatusCode(500, "An error occurred while retrieving posts.");
        }
    }

    // GET: api/posts/5
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PostDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostDto>> GetPost(int id)
    {
        try
        {
            var post = await _context.Posts
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (post == null)
            {
                return NotFound();
            }

            return _mapper.Map<PostDto>(post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting post with ID {id}");
            return StatusCode(500, "An error occurred while retrieving the post.");
        }
    }

    // POST: api/posts
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(PostDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PostDto>> CreatePost([FromBody] CreatePostDto createPostDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var post = _mapper.Map<Post>(createPostDto);
            post.UserId = user.Id;
            post.CreatedAt = DateTime.UtcNow;
            
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            var postDto = _mapper.Map<PostDto>(post);
            return CreatedAtAction(nameof(GetPost), new { id = post.Id }, postDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating post");
            return StatusCode(500, "An error occurred while creating the post.");
        }
    }

    // PUT: api/posts/5
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdatePost(int id, [FromBody] UpdatePostDto updatePostDto)
    {
        try
        {
            if (id != updatePostDto.Id)
            {
                return BadRequest("ID in the URL does not match the ID in the request body");
            }

            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            if (post.UserId != user.Id && !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return Forbid();
            }

            _mapper.Map(updatePostDto, post);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (!await PostExists(id))
            {
                return NotFound();
            }
            _logger.LogError(ex, $"Concurrency error updating post with ID {id}");
            return StatusCode(500, "A concurrency error occurred while updating the post.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating post with ID {id}");
            return StatusCode(500, "An error occurred while updating the post.");
        }
    }

    // DELETE: api/posts/5
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeletePost(int id)
    {
        try
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                return NotFound();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            if (post.UserId != user.Id && !await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return Forbid();
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting post with ID {id}");
            return StatusCode(500, "An error occurred while deleting the post.");
        }
    }

    // GET: api/posts/search
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PostDto>))]
    public async Task<ActionResult<IEnumerable<PostDto>>> SearchPosts(
        [FromQuery] string? title = null, 
        [FromQuery] string? content = null)
    {
        try
        {
            var query = _context.Posts
                .Include(p => p.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(title))
            {
                query = query.Where(p => EF.Functions.Like(p.Title, $"%{title}%"));
            }

            if (!string.IsNullOrWhiteSpace(content))
            {
                query = query.Where(p => EF.Functions.Like(p.Body, $"%{content}%"));
            }

            var posts = await query
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(_mapper.Map<IEnumerable<PostDto>>(posts));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching posts");
            return StatusCode(500, "An error occurred while searching posts.");
        }
    }

    private async Task<bool> PostExists(int id)
    {
        return await _context.Posts.AnyAsync(e => e.Id == id);
    }
}
