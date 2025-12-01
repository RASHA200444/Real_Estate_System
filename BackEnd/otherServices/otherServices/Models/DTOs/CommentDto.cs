using System;

namespace CommentAPI.DTOs
{
    public class CommentDto
    {
        public long Comment_Id { get; set; }
        public string Comment_description { get; set; }
        public DateTime date_comment { get; set; }
        public string User_name { get; set; }
        public long Post_Id { get; set; }
    }
}