using System.Net;

namespace Template.Infrastructure.Email;

public static partial class EmailTemplates
{
    private const string BrandLogoUrl =
        "https://ejwjlhb.stripocdn.email/content/guids/CABINET_e8aaee2e1e85e813b7cf60c9c6d5c81e77fbd7cd36722775f861cb298c1583bf/images/group_1.png";

    public static string GetOnboardingEmailBody(string temporaryPassword, string fullName, string loginEmail)
    {
        var name = WebUtility.HtmlEncode(fullName);
        var email = WebUtility.HtmlEncode(loginEmail);
        var pwd = WebUtility.HtmlEncode(temporaryPassword);
        return $$"""
            <!DOCTYPE html>
            <html dir="ltr" lang="en">
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>Template — Welcome</title>
            </head>
            <body style="margin:0;padding:0;background-color:#F6F6F6;font-family:arial,'helvetica neue',helvetica,sans-serif;">
              <div style="background-color:#F6F6F6;padding:24px 0;">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;margin:0 auto;background:#ffffff;">
                  <tr>
                    <td style="background-color:#14b8a6;padding:20px;color:#fefdfd;">
                      <table width="100%" role="presentation" cellspacing="0" cellpadding="0">
                        <tr>
                          <td style="vertical-align:middle;">
                            <p style="margin:0;font-size:20px;font-weight:700;line-height:1.4;">Welcome to Template</p>
                            <p style="margin:8px 0 0;font-size:14px;opacity:0.95;">Template</p>
                          </td>
                          <td align="right" style="width:150px;">
                            <img src="{{BrandLogoUrl}}" width="149" height="34" alt="" style="display:block;border:0;"/>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:24px 20px;color:#374151;">
                      <p style="margin:0 0 16px;font-size:16px;font-weight:700;">Hello {{name}},</p>
                      <p style="margin:0 0 16px;font-size:14px;line-height:1.5;color:#333333;">
                        Your account has been created. Sign in with your work email and the temporary password below.
                        For your security, you will be asked to choose a new password after you sign in.
                      </p>
                      <p style="margin:0;font-size:14px;line-height:1.5;color:#333333;"><strong>Sign-in email:</strong> {{email}}</p>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:0 20px 24px;">
                      <div style="background:#f2f6f9;border:1px solid #084c92;border-radius:8px;overflow:hidden;text-align:center;">
                        <div style="background:#cafff9;padding:28px 20px;">
                          <p style="margin:0;font-size:14px;line-height:1.4;color:#374151;font-weight:700;">Temporary password</p>
                          <p style="margin:12px 0 0;font-size:36px;font-weight:700;letter-spacing:2px;color:#15b7a5;line-height:1.3;">{{pwd}}</p>
                        </div>
                      </div>
                      <p style="margin:16px 0 0;font-size:13px;color:#6c7278;line-height:1.5;">
                        <em>Keep this message private. If you did not expect this account, contact your administrator.</em>
                      </p>
                    </td>
                  </tr>
                  <tr>
                    <td style="background:#f9fafb;padding:20px;">
                      <p style="margin:0;font-size:14px;color:#6c7278;">© 2026 <strong>Template.</strong> All rights reserved.</p>
                    </td>
                  </tr>
                </table>
              </div>
            </body>
            </html>
            """;
    }

    public static string GetPasswordChangedEmailBody(string? fullName = null)
    {
        var name = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(fullName) ? "there" : fullName.Trim());
        return $$"""
            <!DOCTYPE html>
            <html dir="ltr" lang="en">
            <head>
              <meta charset="UTF-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <title>Template — Password updated</title>
            </head>
            <body style="margin:0;padding:0;background-color:#F6F6F6;font-family:arial,'helvetica neue',helvetica,sans-serif;">
              <div style="background-color:#F6F6F6;padding:24px 0;">
                <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:600px;margin:0 auto;background:#ffffff;">
                  <tr>
                    <td style="background-color:#14b8a6;padding:20px;color:#fefdfd;">
                      <table width="100%" role="presentation" cellspacing="0" cellpadding="0">
                        <tr>
                          <td style="vertical-align:middle;">
                            <p style="margin:0;font-size:20px;font-weight:700;line-height:1.4;">Password updated</p>
                            <p style="margin:8px 0 0;font-size:14px;opacity:0.95;">Template</p>
                          </td>
                          <td align="right" style="width:150px;">
                            <img src="{{BrandLogoUrl}}" width="149" height="34" alt="" style="display:block;border:0;"/>
                          </td>
                        </tr>
                      </table>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:24px 20px;color:#374151;">
                      <p style="margin:0 0 16px;font-size:16px;font-weight:700;">Hello {{name}},</p>
                      <p style="margin:0 0 16px;font-size:14px;line-height:1.5;color:#333333;">
                        Your Template account password was changed successfully. You can sign in with your new password.
                      </p>
                      <p style="margin:0;font-size:14px;line-height:1.5;color:#6c7278;">
                        <em>If you did not make this change, contact your administrator immediately.</em>
                      </p>
                    </td>
                  </tr>
                  <tr>
                    <td style="background:#f9fafb;padding:20px;">
                      <p style="margin:0;font-size:14px;color:#6c7278;">© 2026 <strong>Template.</strong> All rights reserved.</p>
                    </td>
                  </tr>
                </table>
              </div>
            </body>
            </html>
            """;
    }
}
