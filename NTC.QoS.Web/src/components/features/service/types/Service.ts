export type Service = { id: string; name?: string; type?: string };

export type CreateServicePayload = { name: string; type: string };

export type UpdateServicePayload = Partial<CreateServicePayload>;

export type ServiceDetails = Service;
