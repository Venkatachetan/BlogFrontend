using System;
using System.Collections.Generic;
using static MudBlazor.CategoryTypes;


namespace BlogFrontend.Models
{
    public class Post
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public byte[] ImageBytes { get; set; }
        public string ImageBase64 { get; set; }
        public string UserProfilePictureBase64 { get; set; } = string.Empty;
        public List<string> Tags { get; set; }
        public int Likes { get; set; }
        public List<Like> LikedBy { get; set; } = new List<Like>(); 
        public List<Comment> Comments { get; set; } = new List<Comment>();
        public bool ShowCommentForm { get; set; } 
        public string NewComment { get; set; }
    }

    public class Like
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime LikedAt { get; set; }
    }

    public class Comment
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LikeResponse
    {
        public string Message { get; set; }
        public int TotalLikes { get; set; }
        public List<Like> LikedBy { get; set; }
    }

    public class UnlikeResponse
    {
        public string Message { get; set; }
        public int TotalLikes { get; set; }
    }

    public class LikeDetails
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime LikedAt { get; set; }
    }

    public class MessageResponse
    {
        public string Message { get; set; }
        public bool Success { get; set; }

        public string PostId { get; set; } = string.Empty;

    }

    public class User
    {
        public string UserId { get; set; } = "";
        public string Username { get; set; } = "";
        public string? ProfilePictureBase64 { get; set; }
        public bool IsFollowing { get; set; } = false;
        public string ProfilePicture { get; set; }
        public string Bio { get; set; }
    }
    public class ProfileModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ProfilePicture { get; set; } = string.Empty;
    }
    public class GenerateContentResponse
    {
        public string Title { get; set; }
        public string GeneratedContent { get; set; }
    }
    public class ErrorResponse
    {
        public string Message { get; set; }
    }

    public class OperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class PaginatedResponse<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public long TotalCount { get; set; }
    }

    public class SearchResult
    {
        public string Content { get; set; }
        public string Title { get; set; }
        public float SimilarityScore { get; set; }
        public string Id { get; set; }
    }

    public class CombinedSearchResult
    {
        public List<User>? Users { get; set; }
        public List<SearchResult>? Posts { get; set; }
    }
}

