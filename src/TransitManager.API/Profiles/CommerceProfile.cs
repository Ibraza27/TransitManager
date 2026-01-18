using AutoMapper;
using TransitManager.Core.DTOs.Commerce;
using TransitManager.Core.Entities.Commerce;

namespace TransitManager.API.Profiles
{
    public class CommerceProfile : Profile
    {
        public CommerceProfile()
        {
            CreateMap<Product, ProductDto>().ReverseMap();
            CreateMap<Quote, QuoteDto>().ReverseMap();
            CreateMap<QuoteLine, QuoteLineDto>().ReverseMap();
            CreateMap<QuoteHistory, QuoteHistoryDto>().ReverseMap();
            // Add other Commerce mappings as needed
        }
    }
}
