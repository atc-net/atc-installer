# 📦 atc-installer has moved

## 👉 [atc-net-apps/atc-soft-deployment-manager](https://github.com/atc-net-apps/atc-soft-deployment-manager) 👈

🚀 **AtcSoft Deployment Manager** (formerly **Atc.Installer**) is a Windows desktop app that installs, updates, starts and stops the components of a system on an on-premises Windows server. A project file describes the components:

- 🌐 websites on IIS (.NET, Node.js/React, Blazor)
- ⚙️ Windows services and applications
- 🐘 PostgreSQL and 🔎 Elasticsearch
- ☁️ downloads from Azure Blob Storage

Everything in this repository now lives in the new repo, under new names:

| 📁 Here | ➡️ Now in atc-soft-deployment-manager |
|---|---|
| `Atc.Installer.Wpf.App` (the desktop app) | `AtcSoft.Deployment.Manager.Wpf.App` |
| `Atc.Installer.Integration` | `AtcSoft.Deployment.Manager` |
| `Atc.Installer.Integration.*` (Azure, ElasticSearch, InternetInformationServer, PostgreSql, WindowsApplication) | `AtcSoft.Deployment.Manager.Windows` |
| `Atc.Installer.Wpf.ComponentProvider` and `Atc.Installer.Wpf.ComponentProvider.*` | `AtcSoft.Deployment.Manager.Wpf` |
| `Atc.Installer.vdproj` (the MSI) | `setup/AtcSoft.Deployment.Manager.Installer` |
| Tests and sample projects | `test/AtcSoft.Deployment.Manager.*.Tests`, `sample-projects` |

🔄 Moving from Atc.Installer 1.x: see the [migration guide](https://github.com/atc-net-apps/atc-soft-deployment-manager/blob/main/docs/migration-from-1.x.md). Version 2.0 installs next to 1.x, so both can run while you move your projects.

ℹ️ The `Atc.Installer.*` libraries were never published to NuGet, so there are no packages to replace.

💾 The `Atc.Installer.msi` files for 1.x are still on the [releases page](https://github.com/atc-net/atc-installer/releases).

🧹 The code has been removed from this repository. To see the last version that was here, browse commit [`094bba4`](https://github.com/atc-net/atc-installer/tree/094bba4).

🗄️ This repository is archived and read-only.
