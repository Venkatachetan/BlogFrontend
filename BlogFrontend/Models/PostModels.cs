
using System;
using System.Collections.Generic;


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

    public class CreatePostResponse
    {
        public Post Post { get; set; }
        public string ImageBase64 { get; set; }
    }

    public class UpdatePostRequest
    {
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImageUrl { get; set; }
        public List<string> Tags { get; set; }
    }

    public class UpdatePostResponse
    {
        public Post Post { get; set; }
        public string ImageBase64 { get; set; }
    }

    public class LikeResponse
    {
        public string Message { get; set; }
        public string LikedBy { get; set; }
        public int TotalLikes { get; set; }
        public List<LikeDetails> Likers { get; set; }
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
        public object Data { get; set; }

        public bool Success { get; set; }
    }

   

}

