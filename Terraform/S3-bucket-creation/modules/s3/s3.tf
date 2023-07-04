resource "aws_s3_bucket" "av_logs_bucket" {
  bucket = "${var.prefix}${var.bucket_name}"
}