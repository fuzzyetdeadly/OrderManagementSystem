export type PaginationOptions = {
  page?: number;
  pageSize?: number;
};

export type ValidationProblemDetails = {
  title: string;
  status: number;
  details?: string;
  errors?: Record<string, string[]>;
};
