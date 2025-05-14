using WordCreationTester;


// Uncomment this to work with the report generation files and test them
// ReportCreator.runGeneration(JsonTests.jsonString2);

// Uncomment this to work with the AI code. 
// AIRunner.runAI();

// Uncomment this to work with the AIService Creator
// await AIServiceCreator.createSearchResource("swin-testing", "swin-testing-ai-programmatic");

// Uncomment this to work with the AIService Destroyer
await AIServiceCreator.DeleteSearchService("swin-testing", "swin-testing-ai-programmatic");