import { Plus } from "lucide-react";

import AddMunicipalDialog from "../features/municipality/dialog/AddMunicipalDialog";
import DeleteMunicipalsDialog from "../features/municipality/dialog/DeleteMunicipalsDialog";
import { Button } from "../ui/button";
import { Input } from "../ui/input";

interface MunicipalToolbarProps {
  search: string;
  onSearch: (query: string) => void;
  selectedCount: number;
  onDelete: () => void;
  onSelectAll: () => void;
  onDeselectAll: () => void;
  allSelected: boolean;
  provinceId?: string;
  onChangeOpen?: (open: boolean) => void;
  open?: boolean;
  municipalSelectedId?: string[];
  isPending?: boolean;
  isMunicipalDeleteDialog?: boolean;
  setIsMunicipalDeleteDialog?: (open: boolean) => void;
}

const MunicipalToolbar = ({
  search,
  onSearch,
  selectedCount,
  onDelete,
  onSelectAll,
  onDeselectAll,
  allSelected,
  provinceId,
  open,
  onChangeOpen,
  isPending,
  setIsMunicipalDeleteDialog,
}: MunicipalToolbarProps) => {
  return (
    <>
      <DeleteMunicipalsDialog isPending={isPending} onDelete={onDelete} count={selectedCount} />
      <AddMunicipalDialog provinceId={provinceId} open={open} onChangeOpen={onChangeOpen} />
      <div className="w-full flex justify-between items-center">
        <Input
          placeholder="Search Municipal..."
          className="w-[300px]"
          value={search}
          onChange={(e) => onSearch(e.target.value)}
        />
        <div className="w-fit flex items-center space-x-2">
          {selectedCount > 0 && (
            <Button variant="outline" onClick={allSelected ? onDeselectAll : onSelectAll}>
              {allSelected ? `Deselect All` : `Select All`}
            </Button>
          )}

          {selectedCount > 0 && (
            <Button
              disabled={selectedCount < 1}
              variant="destructive"
              onClick={() => setIsMunicipalDeleteDialog?.(true)}>
              Delete ({selectedCount})
            </Button>
          )}
          <Button variant="default" onClick={() => onChangeOpen?.(true)}>
            <Plus /> Add Municipal
          </Button>
        </div>
      </div>
    </>
  );
};

export default MunicipalToolbar;
