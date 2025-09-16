# Signal Strength Helper & Extensions Usage Examples

The signal strength utilities have been moved to the Domain layer for reuse throughout the codebase.

## Available Classes

### 1. SignalStrengthHelper (Static Helper Class)
- **Location**: `NTC.QoS.Domain.Helper.SignalStrengthHelper`
- **Purpose**: Core signal strength operations and calculations
- **Usage**: Direct static method calls

### 2. SignalStrengthExtensions (Extension Methods)
- **Location**: `NTC.QoS.Domain.Extensions.SignalStrengthExtensions`
- **Purpose**: Fluent API for signal strength operations
- **Usage**: Extension methods on decimal, string, and SignalStrengthEnum types

## Usage Examples

### Using the Helper Class

```csharp
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Enums;

// Classify RSSI values
decimal rssi = 85;
SignalStrengthEnum strength = SignalStrengthHelper.ClassifyRSSI(rssi);
// Result: SignalStrengthEnum.Weak

// Parse signal quality strings
string quality = "good";
SignalStrengthEnum parsed = SignalStrengthHelper.ParseSignalQualityString(quality);
// Result: SignalStrengthEnum.Good

// Get human-readable descriptions
string description = SignalStrengthHelper.GetDescription(SignalStrengthEnum.Good);
// Result: "Good"

// Calculate average signal strength
var rssiValues = new List<decimal> { 65, 75, 85, 95 };
var qualityStrings = new List<string> { "good", "average", "weak" };
double average = SignalStrengthHelper.CalculateAverageSignalStrength(rssiValues, qualityStrings);
// Result: 3.57

// Check minimum requirements
bool meetsReq = SignalStrengthHelper.MeetsMinimumRequirement(SignalStrengthEnum.Average);
// Result: true
```

### Using Extension Methods

```csharp
using NTC.QoS.Domain.Extensions;
using NTC.QoS.Domain.Enums;

// Fluent RSSI classification
decimal rssi = 65;
SignalStrengthEnum strength = rssi.ToSignalStrength();
string description = strength.GetDescription();
string range = strength.GetRSSIRange();
bool meetsReq = strength.MeetsMinimumRequirement();

// Chain operations
string result = 75m.ToSignalStrength().GetDescription();
// Result: "Average"

// Parse quality strings
SignalStrengthEnum parsed = "weak".ToSignalStrength();
// Result: SignalStrengthEnum.Weak

// Validate RSSI values
bool isValid = 85m.IsValidRSSI();
// Result: true

// Calculate averages from collections
var rssiList = new List<decimal> { 60, 70, 80, 90 };
double avgFromRSSI = rssiList.CalculateAverageSignalStrength();

var qualityList = new List<string> { "good", "average", "weak" };
double avgFromQuality = qualityList.CalculateAverageSignalStrength();

// Get description from score
string scoreDesc = avgFromRSSI.GetSignalStrengthDescription();
```

### Usage in Analytics Handlers

```csharp
// In any handler or service class
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Extensions;

public class SomeAnalyticsHandler
{
    private void ProcessSpeedTestData(List<SpeedTest> tests)
    {
        foreach (var test in tests)
        {
            // Using helper class
            var signalStrength = SignalStrengthHelper.ClassifyRSSI(test.RSSI);
            
            // Using extensions
            var description = test.RSSI.ToSignalStrength().GetDescription();
            var meetsReq = test.SignalQuality.ToSignalStrength().MeetsMinimumRequirement();
            
            // Process based on signal strength
            if (signalStrength.MeetsMinimumRequirement())
            {
                // Include in quality calculations
            }
        }
        
        // Calculate overall averages
        var rssiValues = tests.Select(t => t.RSSI);
        var avgStrength = rssiValues.CalculateAverageSignalStrength();
        var overallRating = avgStrength.GetSignalStrengthDescription();
    }
}
```

### Usage in DTOs and Domain Models

```csharp
public class SpeedTestResultDto
{
    public decimal RSSI { get; set; }
    public string SignalQuality { get; set; }
    
    // Computed properties using extensions
    public SignalStrengthEnum SignalStrengthLevel => RSSI.ToSignalStrength();
    public string SignalStrengthDescription => SignalStrengthLevel.GetDescription();
    public string RSSIRange => SignalStrengthLevel.GetRSSIRange();
    public bool MeetsMinimumSignalRequirement => SignalStrengthLevel.MeetsMinimumRequirement();
}
```

## Benefits

1. **Consistency**: All signal strength calculations use the same logic
2. **Reusability**: Available throughout the entire codebase
3. **Maintainability**: Changes to signal strength logic only need to be made in one place
4. **Type Safety**: Strongly typed enums prevent invalid signal strength values
5. **Fluent API**: Extension methods provide a clean, readable syntax
6. **Validation**: Built-in RSSI validation ensures data quality
7. **Documentation**: Comprehensive XML documentation for all methods
8. **Standards Compliant**: Aligned with NTC Philippines signal strength classifications

## Performance Notes

- All methods are static and lightweight
- Extension methods have minimal overhead
- RSSI validation is fast range checking
- Collections are processed efficiently using LINQ
- No memory allocations except for returned collections