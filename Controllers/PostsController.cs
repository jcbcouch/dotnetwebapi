using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webapi.Data;
using webapi.Models;
using webapi.DTOs;
using AutoMapper;
using System.Net;
using Microsoft.Data.SqlClient;

namespace webapi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PostsController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PostsController> _logger;
    private readonly IMapper _mapper;

    public PostsController(
        ApplicationDbContext context, 
        ILogger<PostsController> logger,
        IMapper mapper)
    {
        _context = context;
        _logger = logger;
        _mapper = mapper;
    }

    // GET: api/posts
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PostDto>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<PostDto>>> GetPosts()
    {
        try
        {
            _logger.LogInformation("Fetching all posts");
            var posts = await _context.Posts.ToListAsync();
            return Ok(_mapper.Map<IEnumerable<PostDto>>(posts));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching posts");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
        }
    }

    // GET: api/posts/5
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PostDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PostDto>> GetPost(int id)
    {
        try
        {
            _logger.LogInformation("Fetching post with ID: {PostId}", id);
            var post = await _context.Posts.FindAsync(id);

            if (post == null)
            {
                _logger.LogWarning("Post with ID {PostId} not found", id);
                return NotFound();
            }

            return _mapper.Map<PostDto>(post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while fetching post with ID: {PostId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
        }
    }

    // POST: api/posts
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(PostDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PostDto>> CreatePost([FromBody] CreatePostDto createPostDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state: {ModelState}", ModelState);
                return BadRequest(ModelState);
            }

            var post = _mapper.Map<Post>(createPostDto);
            post.CreatedAt = DateTime.UtcNow;
            
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new post with ID: {PostId}", post.Id);
            var postDto = _mapper.Map<PostDto>(post);
            return CreatedAtAction(nameof(GetPost), new { id = post.Id }, postDto);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            _logger.LogWarning(ex, "Duplicate post creation attempted");
            return BadRequest("A post with the same title already exists.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating a post");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the post.");
        }
    }

    // PUT: api/posts/5
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdatePost(int id, [FromBody] UpdatePostDto updatePostDto)
    {
        try
        {
            if (id != updatePostDto.Id)
            {
                _logger.LogWarning("ID mismatch in update request. URL ID: {UrlId}, Body ID: {BodyId}", id, updatePostDto.Id);
                return BadRequest("ID in the URL does not match the ID in the request body");
            }

            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                _logger.LogWarning("Post with ID {PostId} not found for update", id);
                return NotFound();
            }

            _mapper.Map(updatePostDto, post);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated post with ID: {PostId}", id);

            return NoContent();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            if (!await PostExists(id))
            {
                _logger.LogWarning("Concurrency conflict: Post with ID {PostId} not found", id);
                return NotFound();
            }
            _logger.LogError(ex, "Concurrency error while updating post with ID: {PostId}", id);
            return StatusCode(StatusCodes.Status409Conflict, "The record you attempted to update was modified by another user.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating post with ID: {PostId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the post.");
        }
    }

    // DELETE: api/posts/5
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeletePost(int id)
    {
        try
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null)
            {
                _logger.LogWarning("Post with ID {PostId} not found for deletion", id);
                return NotFound();
            }

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Deleted post with ID: {PostId}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting post with ID: {PostId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the post.");
        }
    }

    // GET: api/posts/search
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PostDto>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<PostDto>>> SearchPosts(
        [FromQuery] string? title = null, 
        [FromQuery] string? content = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(content))
            {
                _logger.LogWarning("Search called without any search parameters");
                return BadRequest("At least one search parameter (title or content) is required.");
            }

            var query = _context.Posts.AsQueryable();

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

            _logger.LogInformation("Search completed. Found {Count} matching posts", posts.Count);
            return Ok(_mapper.Map<IEnumerable<PostDto>>(posts));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while searching posts. Title: {Title}, Content: {Content}", title, content);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while searching for posts.");
        }
    }

    private async Task<bool> PostExists(int id)
    {
        return await _context.Posts.AnyAsync(e => e.Id == id);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException is SqlException sqlEx && 
               (sqlEx.Number == 2601 || sqlEx.Number == 2627);
    }
}
