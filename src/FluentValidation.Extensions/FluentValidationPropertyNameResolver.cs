﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using AutoMapper;
using AutoMapper.Internal;
using Stef.Validation;

namespace FluentValidation.Extensions.AutoMapper;

/// <summary>
/// Implementation from IFluentValidationPropertyNameResolver
/// </summary>
/// <seealso cref="IFluentValidationPropertyNameResolver" />
public class FluentValidationPropertyNameResolver : IFluentValidationPropertyNameResolver
{
    private readonly IDictionary<(Type dtoType, string dtoProperty), string> _mappings = new Dictionary<(Type dtoType, string dtoProperty), string>();

    /// <summary>
    /// Initializes a new instance of the <see cref="FluentValidationPropertyNameResolver"/> class.
    /// </summary>
    /// <param name="mapper">The mapper.</param>
    public FluentValidationPropertyNameResolver(IMapper mapper)
    {
        Init(Guard.NotNull(mapper));
    }

    private void Init(IMapper mapper)
    {
#if AUTOMAPPER11
        var configuration = mapper.ConfigurationProvider;
        var configurationType = configuration.GetType();
        var fieldInfo = configurationType.GetField("_configuredMaps", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var configuredMaps = (Dictionary<TypePair, TypeMap>)fieldInfo.GetValue(configuration)!;
        var allTypeMaps = configuredMaps.Values;
#else
        var allTypeMaps = mapper.ConfigurationProvider.GetAllTypeMaps();
#endif
        foreach (TypeMap map in allTypeMaps)
        {
#if AUTOMAPPER11 || AUTOMAPPER8
            var propertyMaps = map.PropertyMaps;
#else
            var propertyMaps = map.GetPropertyMaps();
#endif
            foreach (PropertyMap propertyMap in propertyMaps)
            {
#if AUTOMAPPER11 || AUTOMAPPER8
                string destinationName = propertyMap.DestinationName;
#else
                string destinationName = propertyMap.DestinationProperty.Name;
#endif
                string modelMemberName = propertyMap.SourceMember?.Name!;

                (Type, string) key = (map.DestinationType, destinationName);
                _mappings.Add(key, modelMemberName);
            }
        }
    }

    /// <inheritdoc cref="IFluentValidationPropertyNameResolver.Resolve(Type, MemberInfo, LambdaExpression)"/>
    public string Resolve(Type type, MemberInfo memberInfo, LambdaExpression lambdaExpression)
    {
        string propertyName = memberInfo.Name;

        (Type, string) key = (type, propertyName);

        // Not found in dictionary, just return memberInfo.Name or null.
        return _mappings.ContainsKey(key) ? _mappings[key] : propertyName;
    }
}