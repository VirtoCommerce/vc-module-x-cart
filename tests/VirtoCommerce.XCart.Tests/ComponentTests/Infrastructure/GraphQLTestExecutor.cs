using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using VirtoCommerce.Xapi.Core.Infrastructure;

namespace VirtoCommerce.XCart.Tests.ComponentTests.Infrastructure
{
    /// <summary>
    /// Wraps <see cref="IDocumentExecuter"/> and <see cref="ISchema"/> to provide a simple API
    /// for executing GraphQL queries/mutations in component tests, the same way the ASP.NET
    /// GraphQL middleware would in production. Returns the serialized result via
    /// <see cref="GraphQLTestResult"/>.
    /// </summary>
    public sealed class GraphQLTestExecutor
    {
        private readonly IDocumentExecuter _documentExecuter;
        private readonly ISchema _schema;
        private readonly IServiceProvider _serviceProvider;

        public GraphQLTestExecutor(IDocumentExecuter documentExecuter, ISchema schema, IServiceProvider serviceProvider)
        {
            _documentExecuter = documentExecuter;
            _schema = schema;
            _serviceProvider = serviceProvider;
        }

        public async Task<GraphQLTestResult> ExecuteAsync(
            string query,
            string userId = "test-user",
            string userName = null,
            Dictionary<string, object> variables = null)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, userId),
            };

            if (userName != null)
            {
                claims.Add(new Claim(ClaimTypes.Name, userName));
            }

            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
            var userContext = new GraphQLUserContext(principal);

            var result = await _documentExecuter.ExecuteAsync(options =>
            {
                options.Schema = _schema;
                options.Query = query;
                options.UserContext = userContext;
                options.RequestServices = _serviceProvider;

                if (variables != null)
                {
                    options.Variables = new Inputs(variables);
                }
            });

            var data = ResolveExecutionResult(result);

            return new GraphQLTestResult(result.Errors, data);
        }

        /// <summary>
        /// Serializes the <see cref="ExecutionResult"/> through the registered GraphQL serializer
        /// (matching what the ASP.NET middleware would produce), then returns the parsed "data" object.
        /// </summary>
        private JObject ResolveExecutionResult(ExecutionResult result)
        {
            var serializer = _serviceProvider.GetRequiredService<IGraphQLTextSerializer>();
            var json = serializer.Serialize(result);
            var parsed = JObject.Parse(json);

            return parsed["data"] as JObject;
        }
    }

    /// <summary>
    /// Holds the result of a GraphQL execution. <see cref="Data"/> is the parsed "data" object
    /// from the JSON response (null when the operation failed before producing data).
    /// </summary>
    public sealed record GraphQLTestResult(ExecutionErrors Errors, JObject Data);
}
