import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree, Router } from '@angular/router';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
/**
 *
 */
constructor(private router: Router) {
 
}
  canActivate(
    next: ActivatedRouteSnapshot,
    state: RouterStateSnapshot): boolean{
      if(localStorage.getItem('accessToken') != null){
        return true;
      }
      else{
        this.router.navigateByUrl('/login');
        return false;
      }
   
  }
  
}
