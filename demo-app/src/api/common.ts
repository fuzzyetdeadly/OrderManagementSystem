export const PaginationOptions{
	page?: number,
	pageSize?: number
}

export interface ValidationProblemDetails {
	title: string;
	status: number;
	errors: Record<string, string[]>;
}