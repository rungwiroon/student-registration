using System.Text.RegularExpressions;
using CsrApi.Models;

namespace CsrApi.Services;

public interface IMaskingService
{
    MaskedStudentDto Mask(Student student, string plainName, string plainPhone);
}

public class MaskingService : IMaskingService
{
    public MaskedStudentDto Mask(Student student, string plainName, string plainPhone)
    {
        return new MaskedStudentDto
        {
            Id = student.Id,
            MaskedStudentId = MaskStudentId(student.StudentId),
            MaskedName = MaskName(plainName),
            MaskedPhone = MaskPhone(plainPhone),
            Status = student.Status
        };
    }

    private string MaskStudentId(string studentId)
    {
        if (string.IsNullOrEmpty(studentId) || studentId.Length < 3) return studentId;
        // Example logic: 30***3
        return studentId.Substring(0, 2) + new string('*', studentId.Length - 3) + studentId.Substring(studentId.Length - 1);
    }

    private string MaskName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;
        var parts = name.Split(' ');
        if (parts.Length == 1) return MaskString(name, 2);
        
        // คุณอรรถพล ภู*******
        var firstName = parts[0];
        var lastName = parts[1];
        
        var maskedLastName = lastName.Length > 2 
            ? lastName.Substring(0, 2) + new string('*', lastName.Length - 2) 
            : lastName;

        return $"คุณ{firstName} {maskedLastName}";
    }

    private string MaskPhone(string phone)
    {
        if (string.IsNullOrEmpty(phone)) return phone;
        // 081-XXX-5678 (Assumes 10 digits without dash first)
        phone = phone.Replace("-", "");
        if (phone.Length == 10)
        {
            return $"{phone.Substring(0, 3)}-XXX-{phone.Substring(6, 4)}";
        }
        return MaskString(phone, 3);
    }

    private string MaskString(string input, int visibleChars)
    {
        if (input.Length <= visibleChars) return new string('*', input.Length);
        return input.Substring(0, visibleChars) + new string('*', input.Length - visibleChars);
    }
}
