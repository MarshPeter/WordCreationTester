using CsvParser.Configuration;
using CsvParser.DTO;
using CsvParser.QueryTemplates;
using CSVParser.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CsvParser.Services
{


    public class IndexSeedingService
    {
        // This has been setup so that even if you have no use for these in the future, entries shouldn't be removed.
        // IndexCreatorService checks for mismatches between this list and seeding to determine what AI Search indexes need to be cleaned up. 
        // CSVUploaderService checcks for mismatches between this list and seeding to determine what Folders no longer need to be kept. 
        private static readonly List<IndexDefinition> fullIndexList =
        [
            new IndexDefinition(
                "assurances",
                "Assurances",
                "Contains information regarding comments made regarding assurance practices",
                typeof(AssuranceQueryService),
                typeof(AssuranceCsvRow)),

            new IndexDefinition(
                "issues-actions-tasks",
                "Issues/Actions/Tasks",
                "Contains information about issues, actions, and tasks",
                typeof(IssuesActionTasksQueryService),
                typeof(IssuesActionTasksCsvRow)),

            new IndexDefinition(
                "complaints-and-complements",
                "Complaints And Complements",
                "Contains information about received complaints and complements",
                typeof(ComplaintsOrComplimentsQueryService),
                typeof(ComplaintsOrComplimentsCsvRow))
        ];

        private readonly TMRRadzenContext _dbContext;
        private readonly ILogger<IndexCreatorService> _logger;
        private readonly AIConfig _settings;

        // Creates and manages Azure Cognitive Search indexes, data sources, skillsets, and indexers
        public IndexSeedingService(
            TMRRadzenContext dbContext,
            ILogger<IndexCreatorService> logger,
            IOptions<AIConfig> settings)
        {
            _dbContext = dbContext;
            _logger = logger;
            _settings = settings.Value;
        }


        // TODO: Finalize how this seeding process works. 
        public List<IndexDefinition> GetTenantSeededIndexes(List<string> seeds)
        {
            var tenantIndexes = new List<IndexDefinition>();

            foreach (var index in fullIndexList)
            {
                if (seeds.Contains(index.IndexName))
                {
                    tenantIndexes.Add(index);
                }
            }

            return tenantIndexes;
        }

        // This is to remove any that are not assigned to this tenant, and make sure that there is nothing on the AISearchService that uses them
        public List<IndexDefinition> GetNonTenantSeededIndexes(List<string> seeds)
        {
            var notTenantIndexes = new List<IndexDefinition>();

            foreach (var index in fullIndexList)
            {
                if (!seeds.Contains(index.IndexName))
                {
                    notTenantIndexes.Add(index);
                }
            }

            return notTenantIndexes;
        }
    }
}
