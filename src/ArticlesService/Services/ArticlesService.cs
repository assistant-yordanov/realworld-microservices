﻿using System.Collections.Generic;
using System.Threading.Tasks;
using ArticlesService.Domain.Entities;
using ArticlesService.Domain.Repositories;
using ArticlesService.Protos;
using AutoMapper;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Optional;
using Optional.Async.Extensions;
using YngStrs.Common;

namespace ArticlesService.Services
{
    public class ArticlesService : ArticleService.ArticleServiceBase
    {
        private readonly IMapper _mapper;
        private readonly IArticlesRepository _repository;

        public ArticlesService(IMapper mapper, IArticlesRepository repository)
        {
            _mapper = mapper;
            _repository = repository;
        }

        public override async Task<ArticleView> Publish(PublishArticle command, ServerCallContext context)
        {
            var article = _mapper.Map<Article>(command);
            var entity = await _repository.PersistAsync(article);
            var result = _mapper.Map<ArticleView>(entity);
            return result;
        }

        public override Task<ArticlesView> GetAll(Empty request, ServerCallContext context)
        {
            var results = _mapper.Map<IEnumerable<ArticleView>>(_repository.All);
            return Task.FromResult(new ArticlesView
            {
                Items = { results }
            });
        }

        public override async Task<BySlugResult> GetBySlug(BySlug query, ServerCallContext context) =>
            (await _repository
                .GetBySlugOrErrorAsync(query.Slug)
                .FlatMapAsync(MapSlugResultByArticleOrErrorAsync))
            .ValueOr(Empty);

        private static BySlugResult Empty => new BySlugResult
        {
            HasResult = false,
            View = null
        };

        private Task<Option<BySlugResult, Error>> MapSlugResultByArticleOrErrorAsync(Article entity)
        {
            var result = _mapper.Map<ArticleView>(entity);
            return Task.FromResult(new BySlugResult
            {
                HasResult = true,
                View = result
            }.Some<BySlugResult, Error>());
        }
    }
}