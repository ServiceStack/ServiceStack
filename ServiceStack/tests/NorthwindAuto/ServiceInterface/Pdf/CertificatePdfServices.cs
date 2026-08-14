using MyApp.ServiceModel.Pdf;
using ServiceStack;
using ServiceStack.AI;

namespace MyApp.ServiceInterface.Pdf;

[Route("/certificate/pdf")]
public class GetCertificateOfParticipationPdf : IGet, IReturn<byte[]>
{
    [ValidateNotEmpty]
    public string Name { get; set; }
}

public class CertificateOfParticipationPdfServices(IPdfRenderer pdf) : Service
{
    public async Task<object> Any(GetCertificateOfParticipationPdf request)
    {
        // 1. Map it onto the PDF model. Only you know how your tables relate to the document,
        //    so this part is yours to write — but populate every member: one you leave unset is
        //    omitted from the JSON entirely and typst fails on the missing key.
        var certificateOfParticipation = new CertificateOfParticipation
        {
            Title = "Certificate of Participation",
            Brand = "ACME CO",
            AwardIntro = "This certificate is proudly awarded to",
            Recipient = request.Name,
            Recognition = "in recognition of his/her participation in the",
            Program = "Student Innovation and Leadership Development Program",
            HeldOn = "held on",
            Date = DateTime.Today,
            LeftName = "Jamie Chastain",
            LeftRole = "Program Director",
            RightName = "Benjamin Shah",
            RightRole = "Workshop Trainer",
        };

        // 3. Return it as a download. [Pdf("certificate")] on CertificateOfParticipation picks the
        //    template, so the name isn't repeated here. inline:true shows it in the browser.
        return await pdf.PdfResultAsync(certificateOfParticipation, $"certificate-{request.Name}.pdf");
    }
}
