import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatTabsModule } from '@angular/material/tabs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { HrInventoryService } from '../../core/services/hr-inventory.service';
import { TranslationService } from '../../core/i18n/translation.service';
import { ToastService } from '../../core/services/toast.service';
import {
  InventoryCategoryDto,
  InventoryItemDto,
  StockTransactionDto
} from '../../core/models/inventory.model';
import { HasPermissionDirective } from '../../shared/directives/has-permission.directive';

@Component({
  selector: 'app-inventory',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatTableModule,
    MatTabsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    HasPermissionDirective
  ],
  template: `
    <div class="hp-page-header">
      <div>
        <h1 class="hp-page-title">{{ i18n.t('nav.inventory') }}</h1>
        <p class="hp-page-subtitle">{{ i18n.lang() === 'ar' ? 'إدارة الأصناف، الموردين، أوامر الشراء، وحركات المخزون' : 'Manage inventory items, stock movements, and low-stock alerts' }}</p>
      </div>
    </div>

    <mat-tab-group>
      <mat-tab [label]="i18n.lang() === 'ar' ? 'الأصناف' : 'Items'">
        <div class="hp-card" style="padding:20px;margin:14px 0;">
          <form [formGroup]="itemForm" (ngSubmit)="createItem()" style="display:flex;flex-wrap:wrap;gap:12px;align-items:center;">
            <mat-form-field appearance="outline" style="width:120px;"><mat-label>SKU</mat-label><input matInput formControlName="sku" /></mat-form-field>
            <mat-form-field appearance="outline" style="min-width:180px;"><mat-label>Name</mat-label><input matInput formControlName="name" /></mat-form-field>
            <mat-form-field appearance="outline" style="min-width:160px;">
              <mat-label>Category</mat-label>
              <mat-select formControlName="categoryId">
                @for (c of categories(); track c.id) { <mat-option [value]="c.id">{{ c.name }}</mat-option> }
              </mat-select>
            </mat-form-field>
            <mat-form-field appearance="outline" style="width:110px;"><mat-label>Unit</mat-label><input matInput formControlName="unitOfMeasure" /></mat-form-field>
            <mat-form-field appearance="outline" style="width:110px;"><mat-label>Reorder</mat-label><input matInput type="number" formControlName="reorderLevel" /></mat-form-field>
            <mat-form-field appearance="outline" style="width:110px;"><mat-label>Cost</mat-label><input matInput type="number" formControlName="unitCost" /></mat-form-field>
            <button *appHasPermission="'Inventory.Create'" mat-flat-button class="hp-btn-primary" type="submit" [disabled]="itemForm.invalid">Add Item</button>
          </form>
        </div>

        <div class="hp-card" style="padding:16px;overflow-x:auto;">
          <table mat-table [dataSource]="items()" style="width:100%;">
            <ng-container matColumnDef="sku"><th mat-header-cell *matHeaderCellDef>SKU</th><td mat-cell *matCellDef="let i" style="font-weight:700;">{{ i.sku }}</td></ng-container>
            <ng-container matColumnDef="name"><th mat-header-cell *matHeaderCellDef>Item</th><td mat-cell *matCellDef="let i">{{ i.name }} ({{ i.categoryName }})</td></ng-container>
            <ng-container matColumnDef="stock">
              <th mat-header-cell *matHeaderCellDef>Stock</th>
              <td mat-cell *matCellDef="let i">
                <span class="hp-badge" [class.hp-badge-danger]="i.isLowStock" [class.hp-badge-success]="!i.isLowStock">
                  {{ i.currentStock }} {{ i.unitOfMeasure }}
                </span>
              </td>
            </ng-container>
            <tr mat-header-row *matHeaderRowDef="['sku','name','stock']"></tr>
            <tr mat-row *matRowDef="let row; columns: ['sku','name','stock'];"></tr>
          </table>
        </div>
      </mat-tab>

      <mat-tab [label]="i18n.lang() === 'ar' ? 'حركات المخزون' : 'Stock Transactions'">
        <div class="hp-card" style="padding:20px;margin:14px 0;">
          <form [formGroup]="txForm" (ngSubmit)="recordTx()" style="display:flex;flex-wrap:wrap;gap:12px;align-items:center;">
            <mat-form-field appearance="outline" style="min-width:200px;">
              <mat-label>Item</mat-label>
              <mat-select formControlName="inventoryItemId">
                @for (i of items(); track i.id) { <mat-option [value]="i.id">{{ i.name }} ({{ i.currentStock }})</mat-option> }
              </mat-select>
            </mat-form-field>
            <mat-form-field appearance="outline" style="min-width:170px;">
              <mat-label>Type</mat-label>
              <mat-select formControlName="transactionType">
                <mat-option [value]="1">Purchase In</mat-option>
                <mat-option [value]="2">Department Issue</mat-option>
                <mat-option [value]="3">Adjustment In</mat-option>
                <mat-option [value]="4">Adjustment Out</mat-option>
              </mat-select>
            </mat-form-field>
            <mat-form-field appearance="outline" style="width:120px;"><mat-label>Qty</mat-label><input matInput type="number" formControlName="quantity" /></mat-form-field>
            <mat-form-field appearance="outline" style="flex:1;min-width:160px;"><mat-label>Notes</mat-label><input matInput formControlName="notes" /></mat-form-field>
            <button *appHasPermission="'Inventory.Edit'" mat-flat-button class="hp-btn-gold" type="submit" [disabled]="txForm.invalid">Record</button>
          </form>
        </div>

        <div class="hp-card" style="padding:16px;overflow-x:auto;">
          <table mat-table [dataSource]="transactions()" style="width:100%;">
            <ng-container matColumnDef="item"><th mat-header-cell *matHeaderCellDef>Item</th><td mat-cell *matCellDef="let t">{{ t.itemName }}</td></ng-container>
            <ng-container matColumnDef="qty"><th mat-header-cell *matHeaderCellDef>Qty</th><td mat-cell *matCellDef="let t" style="font-weight:700;">{{ t.quantity }}</td></ng-container>
            <ng-container matColumnDef="bal"><th mat-header-cell *matHeaderCellDef>Balance After</th><td mat-cell *matCellDef="let t">{{ t.balanceAfter }}</td></ng-container>
            <ng-container matColumnDef="notes"><th mat-header-cell *matHeaderCellDef>Notes</th><td mat-cell *matCellDef="let t">{{ t.notes || '-' }}</td></ng-container>
            <tr mat-header-row *matHeaderRowDef="['item','qty','bal','notes']"></tr>
            <tr mat-row *matRowDef="let row; columns: ['item','qty','bal','notes'];"></tr>
          </table>
        </div>
      </mat-tab>
    </mat-tab-group>
  `
})
export class InventoryComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly inv = inject(HrInventoryService);
  private readonly toast = inject(ToastService);
  readonly i18n = inject(TranslationService);

  readonly categories = signal<InventoryCategoryDto[]>([]);
  readonly items = signal<InventoryItemDto[]>([]);
  readonly transactions = signal<StockTransactionDto[]>([]);

  readonly itemForm = this.fb.nonNullable.group({
    sku: ['', Validators.required],
    name: ['', Validators.required],
    categoryId: ['', Validators.required],
    unitOfMeasure: ['Box', Validators.required],
    reorderLevel: [10, [Validators.required, Validators.min(0)]],
    unitCost: [25, [Validators.required, Validators.min(0)]],
    isActive: [true]
  });

  readonly txForm = this.fb.nonNullable.group({
    inventoryItemId: ['', Validators.required],
    transactionType: [1, Validators.required],
    quantity: [1, [Validators.required, Validators.min(0.01)]],
    notes: ['']
  });

  ngOnInit(): void {
    this.inv.getInventoryCategories().subscribe((c) => this.categories.set(c));
    this.load();
  }

  load(): void {
    this.inv.getInventoryItems().subscribe((r) => this.items.set(r.items));
    this.inv.getStockTransactions().subscribe((r) => this.transactions.set(r.items));
  }

  createItem(): void {
    if (this.itemForm.invalid) return;
    this.inv.createInventoryItem(this.itemForm.getRawValue()).subscribe({
      next: () => {
        this.toast.success('Item added');
        this.load();
      },
      error: (err) => this.toast.error(err?.error?.message || 'Failed')
    });
  }

  recordTx(): void {
    if (this.txForm.invalid) return;
    this.inv.recordStockTransaction(this.txForm.getRawValue()).subscribe({
      next: () => {
        this.toast.success('Stock updated');
        this.load();
      },
      error: (err) => this.toast.error(err?.error?.message || 'Failed')
    });
  }
}

