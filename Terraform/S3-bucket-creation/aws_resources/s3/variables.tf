variable "region" {
  type = string
}

variable "applicationName" {
  type = string
}

variable "bucket_name" {
    type = string
    sensitive = true
}

variable "prefix" {
    type = string
}