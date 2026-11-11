# NuGet Trusted Publishing

The manually dispatched **NuGet Publish** workflow (`nuget-push.yml`) uses
GitHub Actions OIDC and `NuGet/login` to obtain a short-lived NuGet.org API key
immediately before pushing packages. It no longer reads `NUGET_APIKEY`.
Package selection, matching security evidence verification, the package-count
guard and `--skip-duplicate` remain in place.

## One-time account setup

1. Sign in to [NuGet.org Trusted Publishing](https://www.nuget.org/account/trustedpublishing)
   as the user who will create the policy. Select the individual or organization
   that owns the packages as the policy owner.
2. Add a GitHub Actions policy with these values:

   | Field | Value |
   | --- | --- |
   | Repository owner | `ServiceStack` |
   | Repository | `ServiceStack` |
   | Workflow file | `nuget-push.yml` |
   | Environment | Leave empty (the job does not use a GitHub environment) |

   Enter the workflow filename only, without `.github/workflows/`. If package
   scopes are offered, allow the package IDs this workflow publishes and the
   operations needed for new versions (and new packages when applicable).
3. In [repository Actions secrets](https://github.com/ServiceStack/ServiceStack/settings/secrets/actions),
   add `NUGET_USER` containing the **NuGet.org profile username of the policy
   creator**. Use the individual username, even for an organization-owned policy;
   do not use the organization name, email address or an API key.
4. Commit and push the workflow changes before using the workflow on GitHub.

The publishing job has `id-token: write` to request an OIDC token, plus the
existing read access to repository contents and Actions artifacts. The temporary
key is passed to the push step through its environment. Other package feeds keep
their existing authentication.

## Publish and verify

Run **NuGet Pack** and let its matching **Package Security Evidence** run finish
successfully. Then dispatch **NuGet Publish** for the same commit. Optionally
supply that pack run's ID using `pack_run_id`. See the
[security evidence workflow documentation](../build/security/README.md#ci-output-and-promotion)
for artifact selection and verification details.

Check that the OIDC login and NuGet push steps both succeed and that the intended
package versions appear on NuGet.org. The temporary key lasts one hour; a rerun
requests a fresh key. A publish can upload packages, so run it when the selected
packages are ready for release.

After the first successful Trusted Publishing release, remove the obsolete
`NUGET_APIKEY` GitHub secret and revoke the old key on NuGet.org if it still exists.
Check any other repositories using that key before removing a shared organization
secret.

If login fails, check the policy creator's username, selected policy owner,
repository, workflow filename, environment and policy activation status. If an
organization owns the policy, the creator must remain an active member. Policies
pending full activation may need reactivation if their initial seven-day window
expires before the first successful publish. A successful login followed by a
push authorization error can indicate a package ownership or scope mismatch.

References: [NuGet Trusted Publishing setup](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)
and [NuGet/login](https://github.com/NuGet/login).
