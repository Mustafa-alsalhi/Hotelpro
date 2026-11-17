export type DocumentType = 1 | 2 | 3 | 4;

export interface GuestDocumentDto {
  id?: string;
  documentType: DocumentType;
  documentNumber: string;
  issuingCountry?: string;
  expiryDate?: string;
}

export interface GuestDto {
  id: string;
  fullName: string;
  phone: string;
  email?: string;
  nationality?: string;
  dateOfBirth?: string;
  address?: string;
  notes?: string;
  isVip: boolean;
  createdAt: string;
  documents: GuestDocumentDto[];
}

