export interface Document {
  id: string;
  fileName: string;
  contentType: string;
  fileSizeBytes: number;
  status: string;
  uploadedAt: string;
}

export interface TestCaseResponse {
  id: string;
  generatedContent: string;
  totalTestCases: number;
  confidenceScore: number;
  generatedAt: string;
}

export interface GenerateTestCaseRequest {
  documentId: string;
  additionalContext?: string;
}