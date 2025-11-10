import { Component } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { UrlService } from '../../services/url.service';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-url-form',
  standalone: true,
  imports: [ReactiveFormsModule, CommonModule],
  templateUrl: './url-form.component.html',
  styleUrl: './url-form.component.scss'
})
export class UrlFormComponent {
  urlForm: FormGroup;
  shortUrl: string = '';
  copied: boolean = false;

  constructor(private fb: FormBuilder, private urlService: UrlService) {
    this.urlForm = this.fb.group({
      longUrl: ['', [Validators.required]],
      isPrivate: [false]
    });
  }

  shorten() {
    if (this.urlForm.invalid) return;

    const { longUrl, isPrivate } = this.urlForm.value;

    this.urlService.shorten(longUrl, isPrivate).subscribe({
      next: (res: any) => {
        this.shortUrl = res.shortUrl;
      },
      error: (err) => {
        alert('Error: ' + err.error);
      }
    });
  }

  copyToClipboard() {
    if (this.shortUrl) {
      navigator.clipboard.writeText(this.shortUrl);
      this.copied = true;
      setTimeout(() => (this.copied = false), 2000);
    }
  }
}
