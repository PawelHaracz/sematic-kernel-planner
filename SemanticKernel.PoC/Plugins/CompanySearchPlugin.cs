using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace SemanticKernel.PoC.Plugins;

public class CompanySearchPlugin
{
    [KernelFunction,Description("get employee name")]
    public string EmployeeSearch(string input)
    {
        return "Pawel Haracz";
    }

    [KernelFunction,Description("search weather")]
    public string WeatherSearch(string city)
    {
        return city + ", 2 degree,rainy";
    }
}