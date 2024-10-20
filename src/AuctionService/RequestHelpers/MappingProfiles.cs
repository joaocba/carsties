using System;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using Contracts;

namespace AuctionService.RequestHelpers;

public class MappingProfiles :  Profile

{
    public MappingProfiles()
    {
        // Map using AutoMapper
        CreateMap<Auction, AuctionDto>().IncludeMembers(x => x.Item);
        CreateMap<Item, AuctionDto>();
        CreateMap<CreateAuctionDto, Auction>()
            .ForMember(d => d.Item, o => o.MapFrom(s => s));
        CreateMap<CreateAuctionDto, Item>();

        // Map using AutoMapper to Contracts
        CreateMap<AuctionDto, AuctionCreated>();
        CreateMap<Auction, AuctionUpdated>().IncludeMembers(a => a.Item);
        CreateMap<Item, AuctionUpdated>();
    }
}
