using AgentService.Ba;

namespace AgentService.Ba.Tests;

public class SchemaValidatorTests
{
    private readonly SchemaValidator _validator = SchemaValidator.FromFile(RepoPathResolver.ResolveSchemaPath());

    private const string ValidJson = """
    {
      "summary": "Thêm phân trang cho API danh sách task",
      "userStory": "Là một người dùng, tôi muốn danh sách task được phân trang, để hệ thống phản hồi nhanh hơn khi số lượng task lớn.",
      "acceptanceCriteria": [
        "GET /projects/{projectId}/tasks nhận tham số page (mặc định 1) và pageSize (mặc định 20, tối đa 100).",
        "Kết quả trả về gồm items, totalCount, page, pageSize, totalPages.",
        "Trả về lỗi 400 ValidationProblem nếu tham số phân trang không hợp lệ."
      ],
      "openQuestions": [
        "Có cần hỗ trợ sắp xếp tùy chỉnh bên cạnh Id tăng dần không?"
      ],
      "assumptions": [
        "Quyền truy cập vẫn được xác thực qua header X-User-Id như hiện tại."
      ]
    }
    """;

    [Fact]
    public void Validate_ValidSampleOutput_Passes()
    {
        // Act
        var isValid = _validator.Validate(ValidJson, out var analysis, out var errors);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
        Assert.NotNull(analysis);
        Assert.Equal("Thêm phân trang cho API danh sách task", analysis.Summary);
        Assert.Equal(3, analysis.AcceptanceCriteria.Count);
        Assert.Single(analysis.OpenQuestions);
        Assert.Single(analysis.Assumptions);
    }

    [Fact]
    public void Validate_WrappedInMarkdownJsonFence_UnwrapsAndPasses()
    {
        // Arrange
        var wrapped = $"```json\n{ValidJson}\n```";

        // Act
        var isValid = _validator.Validate(wrapped, out var analysis, out var errors);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
        Assert.NotNull(analysis);
        Assert.Equal("Thêm phân trang cho API danh sách task", analysis.Summary);
    }

    [Fact]
    public void Validate_InvalidJson_FailsWithParseError()
    {
        // Arrange
        var brokenJson = "{ invalid json content: true ";

        // Act
        var isValid = _validator.Validate(brokenJson, out var analysis, out var errors);

        // Assert
        Assert.False(isValid);
        Assert.Null(analysis);
        Assert.Contains(errors, e => e.Contains("JSON parse error"));
    }

    [Fact]
    public void Validate_MissingRequiredField_Fails()
    {
        // Arrange: Missing openQuestions
        var json = """
        {
          "summary": "Tóm tắt yêu cầu",
          "userStory": "Là một người dùng...",
          "acceptanceCriteria": ["Tiêu chí 1"],
          "assumptions": []
        }
        """;

        // Act
        var isValid = _validator.Validate(json, out var analysis, out var errors);

        // Assert
        Assert.False(isValid);
        Assert.Null(analysis);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_ExtraField_FailsDueToAdditionalProperties()
    {
        // Arrange: Extra property "unexpectedField"
        var json = """
        {
          "summary": "Tóm tắt yêu cầu",
          "userStory": "Là một người dùng...",
          "acceptanceCriteria": ["Tiêu chí 1"],
          "openQuestions": [],
          "assumptions": [],
          "unexpectedField": "not allowed"
        }
        """;

        // Act
        var isValid = _validator.Validate(json, out var analysis, out var errors);

        // Assert
        Assert.False(isValid);
        Assert.Null(analysis);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_WrongType_Fails()
    {
        // Arrange: acceptanceCriteria is a string instead of an array
        var json = """
        {
          "summary": "Tóm tắt yêu cầu",
          "userStory": "Là một người dùng...",
          "acceptanceCriteria": "Phải là mảng nhưng lại là chuỗi",
          "openQuestions": [],
          "assumptions": []
        }
        """;

        // Act
        var isValid = _validator.Validate(json, out var analysis, out var errors);

        // Assert
        Assert.False(isValid);
        Assert.Null(analysis);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_EmptyAcceptanceCriteria_FailsDueToMinItems()
    {
        // Arrange: acceptanceCriteria has 0 items (minItems is 1)
        var json = """
        {
          "summary": "Tóm tắt yêu cầu",
          "userStory": "Là một người dùng...",
          "acceptanceCriteria": [],
          "openQuestions": [],
          "assumptions": []
        }
        """;

        // Act
        var isValid = _validator.Validate(json, out var analysis, out var errors);

        // Assert
        Assert.False(isValid);
        Assert.Null(analysis);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_StringTooLong_FailsDueToMaxLength()
    {
        // Arrange: summary exceeds 600 characters
        var longSummary = new string('A', 601);
        var json = $$"""
        {
          "summary": "{{longSummary}}",
          "userStory": "Là một người dùng...",
          "acceptanceCriteria": ["Tiêu chí 1"],
          "openQuestions": [],
          "assumptions": []
        }
        """;

        // Act
        var isValid = _validator.Validate(json, out var analysis, out var errors);

        // Assert
        Assert.False(isValid);
        Assert.Null(analysis);
        Assert.NotEmpty(errors);
    }

    [Fact]
    public void Validate_SummaryAllWhitespace_FailsDomainRule()
    {
        // Arrange
        var json = """
        {
          "summary": "     \t \n   ",
          "userStory": "Là một người dùng...",
          "acceptanceCriteria": ["Tiêu chí 1"],
          "openQuestions": [],
          "assumptions": []
        }
        """;

        // Act
        var isValid = _validator.Validate(json, out var analysis, out var errors);

        // Assert
        Assert.False(isValid);
        Assert.Null(analysis);
        Assert.Contains(errors, e => e.Contains("summary"));
    }

    [Fact]
    public void Validate_DuplicateAcceptanceCriteria_FailsDomainRule()
    {
        // Arrange: Duplicate items in acceptanceCriteria
        var json = """
        {
          "summary": "Tóm tắt hợp lệ",
          "userStory": "Là một người dùng...",
          "acceptanceCriteria": [
            "Tiêu chí kiểm tra số 1",
            "Tiêu chí kiểm tra số 1"
          ],
          "openQuestions": [],
          "assumptions": []
        }
        """;

        // Act
        var isValid = _validator.Validate(json, out var analysis, out var errors);

        // Assert
        Assert.False(isValid);
        Assert.Null(analysis);
        Assert.Contains(errors, e => e.Contains("duplicate"));
    }
}
