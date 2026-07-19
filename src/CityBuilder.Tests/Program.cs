using CityBuilder.Tests.Framework;

Console.WriteLine("== CityBuilder.Tests ==");
int failed = TestRunner.RunAll();
return failed == 0 ? 0 : 1;
