#!/bin/bash

# StressLab Performance Test Runner
# Bash script for easy test execution on Linux/macOS

# Default values
SCENARIO=""
DURATION=60
USERS=10
ENVIRONMENT="Development"
SCENARIOS_FILE="scenarios.json"
LIST_SCENARIOS=false
HELP=false

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -s|--scenario)
            SCENARIO="$2"
            shift 2
            ;;
        -d|--duration)
            DURATION="$2"
            shift 2
            ;;
        -u|--users)
            USERS="$2"
            shift 2
            ;;
        -e|--environment)
            ENVIRONMENT="$2"
            shift 2
            ;;
        -f|--file)
            SCENARIOS_FILE="$2"
            shift 2
            ;;
        -l|--list)
            LIST_SCENARIOS=true
            shift
            ;;
        -h|--help)
            HELP=true
            shift
            ;;
        *)
            echo "Unknown option $1"
            exit 1
            ;;
    esac
done

# Display help information
if [ "$HELP" = true ]; then
    echo "StressLab Performance Test Runner"
    echo "================================="
    echo ""
    echo "Usage:"
    echo "  ./run-test.sh -s 'API Performance Test' -d 120 -u 50"
    echo ""
    echo "Parameters:"
    echo "  -s, --scenario      Name of the scenario to execute"
    echo "  -d, --duration      Test duration in seconds (default: 60)"
    echo "  -u, --users         Number of concurrent users (default: 10)"
    echo "  -e, --environment   Environment (Development/Production) (default: Development)"
    echo "  -f, --file          Path to scenarios JSON file (default: scenarios.json)"
    echo "  -l, --list          List all available scenarios"
    echo "  -h, --help          Show this help message"
    echo ""
    echo "Examples:"
    echo "  ./run-test.sh --list"
    echo "  ./run-test.sh -s 'API Performance Test'"
    echo "  ./run-test.sh -s 'Database Performance Test' -d 300 -u 100"
    echo "  ./run-test.sh -s 'System Impact Assessment' -e Production"
    echo ""
    exit 0
fi

# Set environment variable
export ASPNETCORE_ENVIRONMENT="$ENVIRONMENT"

# Build the command
COMMAND="dotnet run --project tests/StressLab.PerformanceTests"

# Add parameters
if [ "$LIST_SCENARIOS" = true ]; then
    COMMAND="$COMMAND -- --list-scenarios"
elif [ -n "$SCENARIO" ]; then
    COMMAND="$COMMAND -- --scenario \"$SCENARIO\""
    COMMAND="$COMMAND --duration $DURATION"
    COMMAND="$COMMAND --users $USERS"
else
    COMMAND="$COMMAND -- --duration $DURATION --users $USERS"
fi

# Display execution information
echo "StressLab Performance Test Runner"
echo "================================="
echo ""
echo "Environment: $ENVIRONMENT"
echo "Scenarios File: $SCENARIOS_FILE"

if [ "$LIST_SCENARIOS" = true ]; then
    echo "Action: List available scenarios"
elif [ -n "$SCENARIO" ]; then
    echo "Scenario: $SCENARIO"
    echo "Duration: $DURATION seconds"
    echo "Users: $USERS"
else
    echo "Action: Run default test"
    echo "Duration: $DURATION seconds"
    echo "Users: $USERS"
fi

echo ""
echo "Command: $COMMAND"
echo ""

# Execute the command
if eval $COMMAND; then
    echo ""
    echo "Test execution completed successfully!"
else
    echo ""
    echo "Test execution failed!"
    exit 1
fi

# Display report locations
REPORTS_PATH="tests/StressLab.PerformanceTests/reports"
if [ -d "$REPORTS_PATH" ]; then
    echo ""
    echo "Reports generated in: $REPORTS_PATH"
    echo "Available reports:"
    
    find "$REPORTS_PATH" -name "*.html" -type f -exec basename {} \; | sort -r
fi

