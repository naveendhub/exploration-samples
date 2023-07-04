provider "aws" {
    default_tags {
        tags = {
        "applicationName" = var.applicationName
        }
  }
  region = var.region
}