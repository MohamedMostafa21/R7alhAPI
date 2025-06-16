using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using R7alaAPI.Data;
using R7alaAPI.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace R7alaAPI.Seeding
{
    public class PlaceSeedDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Location { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public decimal? AveragePrice { get; set; }
        public string Thumbnail { get; set; }
        public string Image1 { get; set; }
        public string Image2 { get; set; }
        public string Image3 { get; set; }
        public string Image4 { get; set; }
        public string Image5 { get; set; }
        public string Image6 { get; set; }
        public string Image7 { get; set; }
        public string Image8 { get; set; }
        public string Image9 { get; set; }
        public string Image10 { get; set; }
    }

    public class SeedPlaces
    {
        private readonly ApplicationDBContext _context;
        private readonly string _seedImagesPath;

        public SeedPlaces(ApplicationDBContext context)
        {
            _context = context;
            _seedImagesPath = Path.Combine(Directory.GetCurrentDirectory(), "Seeding", "SeedImages");
        }

        public async Task SeedAsync()
        {
            var csvPath = Path.Combine(Directory.GetCurrentDirectory(), "Seeding", "PlacesSeedData.csv");

            if (!File.Exists(csvPath))
            {
                Console.WriteLine($"CSV file not found at: {csvPath}");
                return;
            }

            if (!Directory.Exists(_seedImagesPath))
            {
                Console.WriteLine($"Images folder not found at: {_seedImagesPath}");
                return;
            }

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null
            };

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                using var reader = new StreamReader(csvPath);
                using var csv = new CsvReader(reader, config);
                var records = csv.GetRecords<PlaceSeedDto>().ToList();

                foreach (var record in records)
                {
                    if (string.IsNullOrWhiteSpace(record.Name) || string.IsNullOrWhiteSpace(record.City))
                    {
                        Console.WriteLine($"Skipping record: Name or City is empty.");
                        continue;
                    }

                    if (await _context.Places.AnyAsync(p => p.Name == record.Name && p.City == record.City))
                    {
                        Console.WriteLine($"Skipping {record.Name} in {record.City}: Already exists.");
                        continue;
                    }

                    var place = new Place
                    {
                        Name = record.Name,
                        Description = record.Description,
                        Type = record.Type,
                        City = record.City,
                        Country = record.Country,
                        Location = record.Location,
                        Latitude = record.Latitude,
                        Longitude = record.Longitude,
                        AveragePrice = record.AveragePrice,
                        ImageUrls = new List<string>()
                    };

                    // Construct folder path based on place name
                    var folderName = record.Name.Replace(" ", "_");
                    var placeFolderPath = Path.Combine(_seedImagesPath, folderName);

                    // Handle Thumbnail
                    if (!string.IsNullOrEmpty(record.Thumbnail))
                    {
                        var thumbnailPath = Path.Combine(placeFolderPath, record.Thumbnail);
                        if (File.Exists(thumbnailPath))
                        {
                            place.ThumbnailUrl = await CopyFileAsync(thumbnailPath, "places/thumbnails");
                        }
                        else
                        {
                            Console.WriteLine($"Thumbnail not found for {record.Name}: {thumbnailPath}");
                        }
                    }

                    // Handle Additional Images
                    var imageFields = new[] { record.Image1, record.Image2, record.Image3, record.Image4, record.Image5,
                                             record.Image6, record.Image7, record.Image8, record.Image9, record.Image10 };
                    foreach (var imageName in imageFields.Where(img => !string.IsNullOrEmpty(img)))
                    {
                        var imagePath = Path.Combine(placeFolderPath, imageName);
                        if (File.Exists(imagePath))
                        {
                            place.ImageUrls.Add(await CopyFileAsync(imagePath, "places/images"));
                        }
                        else
                        {
                            Console.WriteLine($"Image not found for {record.Name}: {imagePath}");
                        }
                    }

                    _context.Places.Add(place);
                    Console.WriteLine($"Added {record.Name} in {record.City} to database.");
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                Console.WriteLine("Seeding completed.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"Seeding failed: {ex.Message}");
            }
        }

        private async Task<string> CopyFileAsync(string sourcePath, string subfolder)
        {
            var extension = Path.GetExtension(sourcePath)?.ToLowerInvariant();
            if (!new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(extension))
            {
                throw new ArgumentException($"Invalid file format for {sourcePath}. Supported formats: jpg, jpeg, png, gif, webp.");
            }

            var fileInfo = new FileInfo(sourcePath);
            if (fileInfo.Length > 300 * 1024 * 1024)
            {
                throw new ArgumentException($"File size exceeds 300MB limit for {sourcePath}.");
            }

            var uploadsFolder = Path.Combine("wwwroot", "Uploads", subfolder);
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}{extension}";
            var destPath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
            using (var destStream = new FileStream(destPath, FileMode.Create))
            {
                await sourceStream.CopyToAsync(destStream);
            }

            return $"/Uploads/{subfolder}/{uniqueFileName}";
        }
    }
}