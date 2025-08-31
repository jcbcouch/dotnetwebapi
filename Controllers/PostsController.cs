using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webapi.Data;
using webapi.Models;
using webapi.DTOs;
using AutoMapper;

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
    public async Task<ActionResult<IEnumerable<PostDto>>> GetPostsAsync()
    {
        var posts = await _context.Posts.ToListAsync();
        return Ok(_mapper.Map<IEnumerable<PostDto>>(posts));
    }

    // GET: api/posts/5
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PostDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PostDto>> GetPostAsync(int id)
    {
        var post = await _context.Posts.FindAsync(id);

        if (post == null)
        {
            return NotFound();
        }

        return _mapper.Map<PostDto>(post);
    }

    // POST: api/posts
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(PostDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PostDto>> CreatePostAsync([FromBody] CreatePostDto createPostDto)
    {
        var post = _mapper.Map<Post>(createPostDto);
        post.CreatedAt = DateTime.UtcNow;
        
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();

        var postDto = _mapper.Map<PostDto>(post);
        return CreatedAtAction(nameof(GetPostAsync), new { id = post.Id }, postDto);
    }

    // PUT: api/posts/5
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePostAsync(int id, [FromBody] UpdatePostDto updatePostDto)
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

        _mapper.Map(updatePostDto, post);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE: api/posts/5
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePostAsync(int id)
    {
        var post = await _context.Posts.FindAsync(id);
        if (post == null)
        {
            return NotFound();
        }

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
