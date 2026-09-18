namespace StudentPortalAPI.Common;

public static class GradeScale
{
    public const int MaxAssignmentScore = 25;
    public const int MaxMidtermScore = 25;
    public const int MaxFinalScore = 50;

    public static decimal Clamp(decimal score, int maxScore)
    {
        if (score < 0) return 0;
        if (score > maxScore) return maxScore;
        return score;
    }

    public static string CalculateGrade(decimal totalScore)
    {
        return totalScore switch
        {
            >= 90 => "A+",
            >= 85 => "A",
            >= 80 => "A-",
            >= 75 => "B+",
            >= 70 => "B",
            >= 65 => "B-",
            >= 60 => "C+",
            >= 55 => "C",
            >= 50 => "C-",
            >= 45 => "D+",
            >= 40 => "D",
            >= 35 => "D-",
            _ => "F"
        };
    }

    public static decimal CalculateGradePoints(string grade)
    {
        return grade switch
        {
            "A+" => 4.0m,
            "A" => 4.0m,
            "A-" => 3.7m,
            "B+" => 3.3m,
            "B" => 3.0m,
            "B-" => 2.7m,
            "C+" => 2.3m,
            "C" => 2.0m,
            "C-" => 1.7m,
            "D+" => 1.3m,
            "D" => 1.0m,
            "D-" => 0.7m,
            "F" => 0.0m,
            _ => 0.0m
        };
    }
}
