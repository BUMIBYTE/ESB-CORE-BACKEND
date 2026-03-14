using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Beres.Shared.Models
{
    public class RedPayModel
    {
        [BsonId]
        public string? Id { get; set; }

        [BsonElement("Company")]
        public string? Company { get; set; }
        [BsonElement("Category")]
        public string? Category { get; set; }
        [BsonElement("Website")]
        public string? Website { get; set; }
        [BsonElement("PhoneNumber")]
        public string? PhoneNumber { get; set; }
        [BsonElement("PaymentMethod")]
        public string? PaymentMethod { get; set; }
        [BsonElement("Email")]
        public string? Email { get; set; }
        [BsonElement("Amount")]
        public int? Amount { get; set; }
        [BsonElement("Qty")]
        public int? Qty { get; set; }
        [BsonElement("ReferenceId")]
        public string? ReferenceId { get; set; }
        [BsonElement("Delegate")]
        public string? Delegate { get; set; }
        [BsonElement("Diskon")]
        public string? Diskon { get; set; }


        [BsonElement("MemberOrder")]
        public List<CreateMemberOrder>? MemberOrder { get; set; }
    }
}
