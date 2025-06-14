using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using R7alaAPI.Data;
using R7alaAPI.DTO;
using R7alaAPI.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace R7alaAPI.Controllers
{
    [ApiController]
    [Route("api/hotels/{hotelId}/rooms")]
    public class RoomsController : ControllerBase
    {
        private readonly ApplicationDBContext _context;

        public RoomsController(ApplicationDBContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "Admin,TourGuide")]
        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateRoom(int hotelId, [FromForm] RoomCreateDto roomDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var hotel = await _context.Hotels.FindAsync(hotelId);
            if (hotel == null)
                return NotFound(new { message = "Hotel not found" });

            var room = new Room
            {
                HotelId = hotelId,
                Type = roomDto.Type,
                Capacity = roomDto.Capacity,
                PricePerNight = roomDto.PricePerNight,
                Description = roomDto.Description,
                IsAvailable = roomDto.IsAvailable,
                ImageUrls = new List<string>()
            };

            try
            {
                room.ThumbnailUrl = await SaveFile(roomDto.Thumbnail, "rooms/thumbnails");
                if (roomDto.Images != null && roomDto.Images.Any())
                {
                    foreach (var image in roomDto.Images)
                    {
                        room.ImageUrls.Add(await SaveFile(image, "rooms/images"));
                    }
                }

                _context.Rooms.Add(room);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetRoomById), new { hotelId, id = room.Id }, new { message = "Room created successfully", room = MapToRoomDto(room) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to create room: {ex.Message}" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRooms(int hotelId)
        {
            var hotel = await _context.Hotels.FindAsync(hotelId);
            if (hotel == null)
                return NotFound(new { message = "Hotel not found" });

            var rooms = await _context.Rooms
                .Where(r => r.HotelId == hotelId)
                .ToListAsync();

            var roomDtos = rooms.Select(r => MapToRoomDto(r)).ToList();
            return Ok(roomDtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRoomById(int hotelId, int id)
        {
            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.HotelId == hotelId && r.Id == id);

            if (room == null)
                return NotFound(new { message = "Room not found" });

            return Ok(MapToRoomDto(room));
        }

        [Authorize(Roles = "Admin,TourGuide")]
        [HttpPut("{id}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateRoom(int hotelId, int id, [FromForm] RoomUpdateDto roomDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.HotelId == hotelId && r.Id == id);

            if (room == null)
                return NotFound(new { message = "Room not found" });

            try
            {
                if (!string.IsNullOrEmpty(roomDto.Type)) room.Type = roomDto.Type;
                if (roomDto.Capacity.HasValue) room.Capacity = roomDto.Capacity.Value;
                if (roomDto.PricePerNight.HasValue) room.PricePerNight = roomDto.PricePerNight.Value;
                if (!string.IsNullOrEmpty(roomDto.Description)) room.Description = roomDto.Description;
                if (roomDto.IsAvailable.HasValue) room.IsAvailable = roomDto.IsAvailable.Value;

                if (roomDto.Thumbnail != null)
                {
                    if (!string.IsNullOrEmpty(room.ThumbnailUrl))
                        DeleteFile(room.ThumbnailUrl);
                    room.ThumbnailUrl = await SaveFile(roomDto.Thumbnail, "rooms/thumbnails");
                }

                if (roomDto.Images != null && roomDto.Images.Any())
                {
                    foreach (var oldUrl in room.ImageUrls.ToList())
                        DeleteFile(oldUrl);
                    room.ImageUrls.Clear();
                    foreach (var image in roomDto.Images)
                        room.ImageUrls.Add(await SaveFile(image, "rooms/images"));
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Room updated successfully", room = MapToRoomDto(room) });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to update room: {ex.Message}" });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRoom(int hotelId, int id)
        {
            var room = await _context.Rooms
                .FirstOrDefaultAsync(r => r.HotelId == hotelId && r.Id == id);

            if (room == null)
                return NotFound(new { message = "Room not found" });

            try
            {
                if (!string.IsNullOrEmpty(room.ThumbnailUrl))
                    DeleteFile(room.ThumbnailUrl);

                foreach (var url in room.ImageUrls)
                    DeleteFile(url);

                _context.Rooms.Remove(room);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Room deleted successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Failed to delete room: {ex.Message}" });
            }
        }

        [NonAction]
        private async Task<string> SaveFile(IFormFile file, string subfolder)
        {
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (!new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(extension))
                throw new ArgumentException("Invalid file format. Supported formats: jpg, jpeg, png, gif, webp.");

            if (file.Length > 5 * 1024 * 1024)
                throw new ArgumentException("File size exceeds 5MB limit.");

            var uploadsFolder = Path.Combine("wwwroot", "Uploads", subfolder);
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/Uploads/{subfolder}/{uniqueFileName}";
        }

        [NonAction]
        private void DeleteFile(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            var filePath = Path.Combine("wwwroot", url.TrimStart('/'));
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        [NonAction]
        private static RoomDto MapToRoomDto(Room room)
        {
            return new RoomDto
            {
                Id = room.Id,
                HotelId = room.HotelId,
                Type = room.Type,
                Capacity = room.Capacity,
                PricePerNight = room.PricePerNight,
                Description = room.Description,
                IsAvailable = room.IsAvailable,
                ThumbnailUrl = room.ThumbnailUrl,
                ImageUrls = room.ImageUrls
            };
        }
    }
}