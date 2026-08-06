import { type Generation } from "../../generation/types/Generation";

export type Technology = {
  id: string;
  standard: string;
  description?: string;
  generationId: string;
  generation: Generation;
};

export type TechnologyDetails = Technology;
