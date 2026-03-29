using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Beres.Shared.Models
{
    public class User : BaseModel
    {
        [BsonId]
        // [BsonRepresentation(BsonType.ObjectId)]
        public string? Id {get; set;}
        
        [BsonElement("Email")]
        public string? Email {get; set;}

        [BsonElement("FullName")]
        public string? FullName {get; set;}

        [BsonElement("Phone")]
        public string? Phone {get; set;}

        [BsonElement("Image")]
        public string? Image {get; set;}

        [BsonElement("IdRole")]
        public string? IdRole {get; set;}

        [BsonElement("Pin")]
        public string? Pin {get; set;}

        [BsonElement("Otp")]
        public string? Otp {get; set;}

        [BsonElement("Balance")]
        public float? Balance {get; set;}

        [BsonElement("NoNIK")]
        public string? NoNIK {get; set;}
        [BsonElement("Address")]
        public string? Address {get; set;}

        [BsonElement("Point")]
        public float? Point {get; set;}

        [BsonElement("Fcm")]
        public string? Fcm {get; set;}
    }
}